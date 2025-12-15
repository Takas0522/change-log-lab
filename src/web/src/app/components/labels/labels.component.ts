import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { LabelModel, CreateLabelRequest, UpdateLabelRequest } from '../../models';
import { LabelService } from '../../services/label.service';
import { LabelChipComponent } from '../shared/label-chip.component';

@Component({
  selector: 'app-labels',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, LabelChipComponent],
  template: `
    <div class="labels-container">
      <header class="labels-header">
        <h1>Label Management</h1>
        <button class="btn-primary" (click)="showCreateDialog()">
          + Create Label
        </button>
      </header>

      <div class="labels-list" *ngIf="labels().length > 0; else noLabels">
        <div class="label-item" *ngFor="let label of labels()">
          <app-label-chip [label]="label"></app-label-chip>
          <span class="label-name">{{ label.name }}</span>
          <span class="label-color-text">{{ label.color }}</span>
          <div class="label-actions">
            <button class="btn-edit" (click)="showEditDialog(label)">Edit</button>
            <button class="btn-delete" (click)="confirmDelete(label)">Delete</button>
          </div>
        </div>
      </div>

      <ng-template #noLabels>
        <div class="no-labels">
          <p>No labels yet. Create your first label to organize your todos!</p>
        </div>
      </ng-template>

      <!-- Create/Edit Dialog -->
      <div class="dialog-overlay" *ngIf="showDialog()" (click)="closeDialog()">
        <div class="dialog" (click)="$event.stopPropagation()">
          <h2>{{ editingLabel() ? 'Edit Label' : 'Create Label' }}</h2>
          
          <form (submit)="saveLabel($event)">
            <div class="form-group">
              <label for="labelName">Label Name *</label>
              <input
                id="labelName"
                type="text"
                [(ngModel)]="labelForm.name"
                name="name"
                placeholder="Enter label name"
                maxlength="100"
                required>
            </div>

            <div class="form-group">
              <label for="labelColor">Color *</label>
              <div class="color-input-group">
                <input
                  id="labelColor"
                  type="color"
                  [(ngModel)]="labelForm.color"
                  name="color"
                  required>
                <input
                  type="text"
                  [(ngModel)]="labelForm.color"
                  name="colorText"
                  placeholder="#RRGGBB"
                  pattern="^#[0-9A-Fa-f]{6}$"
                  maxlength="7"
                  class="color-text-input">
                <div class="color-preview" [style.background-color]="labelForm.color">
                  <span [style.color]="getContrastColor(labelForm.color)">
                    Preview
                  </span>
                </div>
              </div>
            </div>

            <div class="dialog-actions">
              <button type="button" class="btn-secondary" (click)="closeDialog()">
                Cancel
              </button>
              <button type="submit" class="btn-primary" [disabled]="saving()">
                {{ saving() ? 'Saving...' : 'Save' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .labels-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 24px;
    }

    .labels-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }

    .labels-header h1 {
      margin: 0;
      font-size: 28px;
    }

    .btn-primary {
      padding: 10px 20px;
      background-color: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .labels-list {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .label-item {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px;
      background: white;
      border: 1px solid #e0e0e0;
      border-radius: 8px;
      transition: box-shadow 0.2s;
    }

    .label-item:hover {
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }

    .label-name {
      flex: 1;
      font-weight: 500;
    }

    .label-color-text {
      color: #666;
      font-family: monospace;
    }

    .label-actions {
      display: flex;
      gap: 8px;
    }

    .btn-edit, .btn-delete {
      padding: 6px 12px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 13px;
    }

    .btn-edit {
      background-color: #f0f0f0;
      color: #333;
    }

    .btn-edit:hover {
      background-color: #e0e0e0;
    }

    .btn-delete {
      background-color: #dc3545;
      color: white;
    }

    .btn-delete:hover {
      background-color: #c82333;
    }

    .no-labels {
      text-align: center;
      padding: 48px 24px;
      color: #666;
    }

    .dialog-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
    }

    .dialog {
      background: white;
      border-radius: 8px;
      padding: 24px;
      width: 90%;
      max-width: 500px;
      max-height: 90vh;
      overflow-y: auto;
    }

    .dialog h2 {
      margin-top: 0;
      margin-bottom: 24px;
    }

    .form-group {
      margin-bottom: 20px;
    }

    .form-group label {
      display: block;
      margin-bottom: 8px;
      font-weight: 500;
    }

    .form-group input[type="text"] {
      width: 100%;
      padding: 10px;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-size: 14px;
      box-sizing: border-box;
    }

    .color-input-group {
      display: flex;
      gap: 12px;
      align-items: center;
    }

    .color-input-group input[type="color"] {
      width: 60px;
      height: 40px;
      border: 1px solid #ccc;
      border-radius: 4px;
      cursor: pointer;
    }

    .color-text-input {
      flex: 1;
      padding: 10px;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-family: monospace;
      font-size: 14px;
    }

    .color-preview {
      padding: 8px 16px;
      border-radius: 4px;
      font-size: 14px;
      font-weight: 500;
      white-space: nowrap;
    }

    .dialog-actions {
      display: flex;
      justify-content: flex-end;
      gap: 12px;
      margin-top: 24px;
    }

    .btn-secondary {
      padding: 10px 20px;
      background-color: #6c757d;
      color: white;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
    }

    .btn-secondary:hover {
      background-color: #545b62;
    }
  `]
})
export class LabelsComponent implements OnInit {
  private labelService = inject(LabelService);

  labels = signal<LabelModel[]>([]);
  showDialog = signal(false);
  editingLabel = signal<LabelModel | null>(null);
  saving = signal(false);

  labelForm = {
    name: '',
    color: '#3B82F6'
  };

  ngOnInit(): void {
    this.loadLabels();
  }

  loadLabels(): void {
    this.labelService.getLabels().subscribe({
      next: (labels) => {
        this.labels.set(labels);
      },
      error: (error) => {
        console.error('Error loading labels:', error);
        alert('Failed to load labels');
      }
    });
  }

  showCreateDialog(): void {
    this.editingLabel.set(null);
    this.labelForm = {
      name: '',
      color: '#3B82F6'
    };
    this.showDialog.set(true);
  }

  showEditDialog(label: LabelModel): void {
    this.editingLabel.set(label);
    this.labelForm = {
      name: label.name,
      color: label.color
    };
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.editingLabel.set(null);
  }

  saveLabel(event: Event): void {
    event.preventDefault();

    if (!this.labelForm.name.trim()) {
      alert('Label name is required');
      return;
    }

    if (!this.labelService.isValidHexColor(this.labelForm.color)) {
      alert('Invalid color format. Please use #RRGGBB format');
      return;
    }

    this.saving.set(true);

    const editing = this.editingLabel();
    if (editing) {
      // Update existing label
      const request: UpdateLabelRequest = {
        name: this.labelForm.name.trim(),
        color: this.labelForm.color.toUpperCase()
      };

      this.labelService.updateLabel(editing.id, request).subscribe({
        next: () => {
          this.loadLabels();
          this.closeDialog();
          this.saving.set(false);
        },
        error: (error) => {
          console.error('Error updating label:', error);
          alert('Failed to update label');
          this.saving.set(false);
        }
      });
    } else {
      // Create new label
      const request: CreateLabelRequest = {
        name: this.labelForm.name.trim(),
        color: this.labelForm.color.toUpperCase()
      };

      this.labelService.createLabel(request).subscribe({
        next: () => {
          this.loadLabels();
          this.closeDialog();
          this.saving.set(false);
        },
        error: (error) => {
          console.error('Error creating label:', error);
          alert('Failed to create label');
          this.saving.set(false);
        }
      });
    }
  }

  confirmDelete(label: LabelModel): void {
    if (confirm(`Are you sure you want to delete the label "${label.name}"? This will remove it from all todos.`)) {
      this.labelService.deleteLabel(label.id).subscribe({
        next: () => {
          this.loadLabels();
        },
        error: (error) => {
          console.error('Error deleting label:', error);
          alert('Failed to delete label');
        }
      });
    }
  }

  getContrastColor(hexColor: string): string {
    return this.labelService.getContrastTextColor(hexColor);
  }
}
