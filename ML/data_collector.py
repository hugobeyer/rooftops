import json
import numpy as np
from pathlib import Path
from datetime import datetime

class GameplayDataCollector:
    def __init__(self, data_dir="ML/training_data"):
        self.data_dir = Path(data_dir)
        self.data_dir.mkdir(parents=True, exist_ok=True)
        self.current_session = []
        
    def add_gameplay_data(self, data_dict):
        """
        Add a single gameplay data point
        data_dict should contain:
        - distance
        - death_count
        - perfect_jumps
        - avg_jump_distance
        - tridots_collected
        - time_alive
        - skill_level
        - completion_rate
        - current_difficulty: dict with game_speed, gap_size, etc.
        """
        data_dict['timestamp'] = datetime.now().isoformat()
        self.current_session.append(data_dict)
    
    def save_session(self):
        if not self.current_session:
            return
            
        filename = f"gameplay_data_{datetime.now().strftime('%Y%m%d_%H%M%S')}.json"
        with open(self.data_dir / filename, 'w') as f:
            json.dump(self.current_session, f, indent=2)
        self.current_session = []
    
    @staticmethod
    def load_all_data(data_dir="ML/training_data"):
        data_dir = Path(data_dir)
        all_data = []
        for file in data_dir.glob("gameplay_data_*.json"):
            with open(file) as f:
                all_data.extend(json.load(f))
        return all_data