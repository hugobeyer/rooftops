import numpy as np
import torch
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import train_test_split

class GameplayDataPreprocessor:
    def __init__(self):
        self.scaler = StandardScaler()
        
    def prepare_data(self, raw_data):
        # Extract features and targets
        features = np.array([[
            d['distance'],
            d['death_count'],
            d['perfect_jumps'],
            d['avg_jump_distance'],
            d['tridots_collected'],
            d['time_alive'],
            d['skill_level'],
            d['completion_rate']
        ] for d in raw_data])
        
        targets = np.array([[
            d['current_difficulty']['game_speed'],
            d['current_difficulty']['gap_size'],
            d['current_difficulty']['height_variation'],
            d['current_difficulty']['tridot_freq'],
            d['current_difficulty']['jump_pad_freq'],
            d['current_difficulty']['prop_freq']
        ] for d in raw_data])
        
        # Normalize features
        features_normalized = self.scaler.fit_transform(features)
        
        # Convert to PyTorch tensors
        X = torch.FloatTensor(features_normalized)
        y = torch.FloatTensor(targets)
        
        # Split data
        X_train, X_val, y_train, y_val = train_test_split(
            X, y, test_size=0.2, random_state=42
        )
        
        return X_train, X_val, y_train, y_val