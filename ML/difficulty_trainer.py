import torch
import torch.nn as nn
import numpy as np
from collections import namedtuple

DifficultyParams = namedtuple('DifficultyParams', [
    'game_speed', 'gap_size', 'height_variation',
    'tridot_freq', 'jump_pad_freq', 'prop_freq'
])

class DifficultyNet(nn.Module):
    def __init__(self):
        super(DifficultyNet, self).__init__()
        self.lstm = nn.LSTM(input_size=8, hidden_size=128, num_layers=2)
        self.fc1 = nn.Linear(128, 64)
        self.fc2 = nn.Linear(64, 6)  # 6 difficulty parameters
        
    def forward(self, x, hidden):
        x, hidden = self.lstm(x, hidden)
        x = torch.relu(self.fc1(x))
        return self.fc2(x), hidden

class DifficultyTrainer:
    def __init__(self):
        self.model = DifficultyNet()
        self.optimizer = torch.optim.Adam(self.model.parameters(), lr=0.001)
        self.hidden = None
        
    def process_gameplay_data(self, data):
        # Convert gameplay metrics to tensor
        metrics = torch.tensor([
            data.distance,
            data.death_count,
            data.perfect_jumps,
            data.avg_jump_distance,
            data.tridots_collected,
            data.time_alive,
            data.skill_level,
            data.completion_rate
        ], dtype=torch.float32).unsqueeze(0).unsqueeze(0)
        
        # Get model prediction
        difficulty_params, self.hidden = self.model(metrics, self.hidden)
        
        return DifficultyParams(*difficulty_params.squeeze().tolist())
    
    def train_step(self, gameplay_data, target_engagement):
        predicted_params, _ = self.model(gameplay_data, None)
        loss = self.calculate_engagement_loss(predicted_params, target_engagement)
        
        self.optimizer.zero_grad()
        loss.backward()
        self.optimizer.step()
        
        return loss.item()
    
    @staticmethod
    def calculate_engagement_loss(params, target_engagement):
        # Complex loss function considering multiple factors
        difficulty_variance = torch.var(params)
        progression_smoothness = torch.mean(torch.abs(params[1:] - params[:-1]))
        engagement_diff = (params.mean() - target_engagement).abs()
        
        return engagement_diff + 0.3 * difficulty_variance + 0.2 * progression_smoothness