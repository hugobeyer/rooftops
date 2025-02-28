import torch
from difficulty_trainer import DifficultyNet, DifficultyParams
from data_preprocessor import GameplayDataPreprocessor
import numpy as np

class DifficultyPredictor:
    def __init__(self, model_path="ML/models/best_difficulty_model.pt"):
        self.model = DifficultyNet()
        checkpoint = torch.load(model_path)
        self.model.load_state_dict(checkpoint['model_state_dict'])
        self.model.eval()
        self.preprocessor = GameplayDataPreprocessor()
        
    def predict(self, gameplay_metrics):
        """
        Predict difficulty parameters based on current gameplay metrics
        """
        # Prepare input data
        features = np.array([[
            gameplay_metrics['distance'],
            gameplay_metrics['death_count'],
            gameplay_metrics['perfect_jumps'],
            gameplay_metrics['avg_jump_distance'],
            gameplay_metrics['tridots_collected'],
            gameplay_metrics['time_alive'],
            gameplay_metrics['skill_level'],
            gameplay_metrics['completion_rate']
        ]])
        
        # Normalize features
        features_normalized = self.preprocessor.scaler.transform(features)
        
        # Convert to tensor and predict
        with torch.no_grad():
            X = torch.FloatTensor(features_normalized).unsqueeze(1)
            predictions, _ = self.model(X, None)
            
        # Convert predictions to DifficultyParams
        params = predictions.squeeze().tolist()
        return DifficultyParams(*params)