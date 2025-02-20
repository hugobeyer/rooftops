from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.base_env import ActionTuple
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from collections import deque
import random

# Neural Network for the agent
class RoofTopsNet(nn.Module):
    def __init__(self, input_size, hidden_size, output_size):
        super(RoofTopsNet, self).__init__()
        self.fc1 = nn.Linear(input_size, hidden_size)
        self.fc2 = nn.Linear(hidden_size, hidden_size)
        self.fc3 = nn.Linear(hidden_size, output_size)
        
    def forward(self, x):
        x = torch.relu(self.fc1(x))
        x = torch.relu(self.fc2(x))
        return self.fc3(x)

# Experience replay memory
class ReplayMemory:
    def __init__(self, capacity):
        self.memory = deque(maxlen=capacity)
        
    def push(self, state, action, reward, next_state, done):
        self.memory.append((state, action, reward, next_state, done))
        
    def sample(self, batch_size):
        return random.sample(self.memory, batch_size)
    
    def __len__(self):
        return len(self.memory)

# Training parameters
BATCH_SIZE = 128
GAMMA = 0.99
EPS_START = 1.0
EPS_END = 0.01
EPS_DECAY = 0.995
TARGET_UPDATE = 10
MEMORY_SIZE = 10000
LEARNING_RATE = 0.001

def train():
    # Initialize Unity environment
    env = UnityEnvironment(file_name=None, seed=1, side_channels=[])
    env.reset()
    
    # Get behavior name and spec
    behavior_name = list(env.behavior_specs)[0]
    spec = env.behavior_specs[behavior_name]
    
    # Initialize networks
    input_size = spec.observation_shapes[0][0]  # Size of observation space
    hidden_size = 128
    output_size = spec.action_spec.discrete_branches[0]  # Size of action space
    
    policy_net = RoofTopsNet(input_size, hidden_size, output_size)
    target_net = RoofTopsNet(input_size, hidden_size, output_size)
    target_net.load_state_dict(policy_net.state_dict())
    
    optimizer = optim.Adam(policy_net.parameters(), lr=LEARNING_RATE)
    memory = ReplayMemory(MEMORY_SIZE)
    
    eps = EPS_START
    episode = 0
    
    while True:
        env.reset()
        episode += 1
        
        decision_steps, terminal_steps = env.get_steps(behavior_name)
        state = decision_steps.obs[0]
        
        episode_reward = 0
        done = False
        
        while not done:
            # Select action
            if random.random() > eps:
                with torch.no_grad():
                    state_tensor = torch.FloatTensor(state).unsqueeze(0)
                    action = policy_net(state_tensor).max(1)[1].item()
            else:
                action = random.randrange(output_size)
            
            # Take action in environment
            action_tuple = ActionTuple()
            action_tuple.add_discrete(np.array([[action]]))
            env.set_actions(behavior_name, action_tuple)
            env.step()
            
            # Get next state and reward
            decision_steps, terminal_steps = env.get_steps(behavior_name)
            
            if len(terminal_steps) > 0:
                done = True
                next_state = terminal_steps.obs[0]
                reward = terminal_steps.reward[0]
            else:
                next_state = decision_steps.obs[0]
                reward = decision_steps.reward[0]
            
            episode_reward += reward
            
            # Store transition in memory
            memory.push(state, action, reward, next_state, done)
            state = next_state
            
            # Perform optimization step if enough samples
            if len(memory) >= BATCH_SIZE:
                transitions = memory.sample(BATCH_SIZE)
                batch = list(zip(*transitions))
                
                state_batch = torch.FloatTensor(np.array(batch[0]))
                action_batch = torch.LongTensor(batch[1])
                reward_batch = torch.FloatTensor(batch[2])
                next_state_batch = torch.FloatTensor(np.array(batch[3]))
                done_batch = torch.FloatTensor(batch[4])
                
                # Compute Q(s_t, a)
                current_q = policy_net(state_batch).gather(1, action_batch.unsqueeze(1))
                
                # Compute V(s_{t+1}) for all next states
                next_q = target_net(next_state_batch).max(1)[0].detach()
                target_q = reward_batch + GAMMA * next_q * (1 - done_batch)
                
                # Compute loss and optimize
                loss = nn.MSELoss()(current_q.squeeze(), target_q)
                optimizer.zero_grad()
                loss.backward()
                optimizer.step()
        
        # Update target network
        if episode % TARGET_UPDATE == 0:
            target_net.load_state_dict(policy_net.state_dict())
        
        # Decay epsilon
        eps = max(EPS_END, eps * EPS_DECAY)
        
        print(f"Episode {episode}, Total Reward: {episode_reward}, Epsilon: {eps:.2f}")
        
        # Save model periodically
        if episode % 100 == 0:
            torch.save({
                'policy_net_state_dict': policy_net.state_dict(),
                'target_net_state_dict': target_net.state_dict(),
                'optimizer_state_dict': optimizer.state_dict(),
                'episode': episode,
                'epsilon': eps
            }, f'ML/rooftops_model_ep{episode}.pt')

if __name__ == "__main__":
    train() 