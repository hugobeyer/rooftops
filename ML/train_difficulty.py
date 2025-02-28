import torch
import torch.nn as nn
from torch.utils.data import DataLoader, TensorDataset
from difficulty_trainer import DifficultyNet
from data_collector import GameplayDataCollector
from data_preprocessor import GameplayDataPreprocessor
import matplotlib.pyplot as plt
from pathlib import Path

def train_difficulty_model(
    batch_size=32,
    epochs=100,
    learning_rate=0.001,
    hidden_size=128,
    save_dir="ML/models"
):
    # Create save directory
    save_dir = Path(save_dir)
    save_dir.mkdir(parents=True, exist_ok=True)
    
    # Load and prepare data
    raw_data = GameplayDataCollector.load_all_data()
    preprocessor = GameplayDataPreprocessor()
    X_train, X_val, y_train, y_val = preprocessor.prepare_data(raw_data)
    
    # Create data loaders
    train_dataset = TensorDataset(X_train, y_train)
    val_dataset = TensorDataset(X_val, y_val)
    train_loader = DataLoader(train_dataset, batch_size=batch_size, shuffle=True)
    val_loader = DataLoader(val_dataset, batch_size=batch_size)
    
    # Initialize model and optimizer
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')
    model = DifficultyNet().to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=learning_rate)
    criterion = nn.MSELoss()
    
    # Training loop
    for epoch in range(epochs):
        # Training phase
        model.train()
        train_loss = 0
        for X_batch, y_batch in train_loader:
            X_batch = X_batch.to(device)
            y_batch = y_batch.to(device)
            optimizer.zero_grad()
            output, _ = model(X_batch.unsqueeze(1), None)
            loss = criterion(output, y_batch)
            loss.backward()
            optimizer.step()
            train_loss += loss.item()
        
        # Validation phase
        model.eval()
        val_loss = 0
        with torch.no_grad():
            for X_batch, y_batch in val_loader:
                X_batch = X_batch.to(device)
                y_batch = y_batch.to(device)
                output, _ = model(X_batch.unsqueeze(1), None)
                val_loss += criterion(output, y_batch).item()
        
        # Record losses
        train_loss = train_loss / len(train_loader)
        val_loss = val_loss / len(val_loader)
        train_losses.append(train_loss)
        val_losses.append(val_loss)
        
        print(f"Epoch {epoch+1}/{epochs}, Train Loss: {train_loss:.4f}, Val Loss: {val_loss:.4f}")
        
        # Save best model
        if val_loss < best_val_loss:
            best_val_loss = val_loss
            torch.save({
                'model_state_dict': model.state_dict(),
                'optimizer_state_dict': optimizer.state_dict(),
                'epoch': epoch,
                'val_loss': val_loss
            }, save_dir / 'best_difficulty_model.pt')
    
    # Plot training progress
    plt.figure(figsize=(10, 5))
    plt.plot(train_losses, label='Training Loss')
    plt.plot(val_losses, label='Validation Loss')
    plt.xlabel('Epoch')
    plt.ylabel('Loss')
    plt.legend()
    plt.savefig(save_dir / 'training_progress.png')
    plt.close()

if __name__ == "__main__":
    train_difficulty_model()
