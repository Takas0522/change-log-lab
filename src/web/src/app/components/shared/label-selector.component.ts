import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LabelModel } from '../../models';
import { LabelService } from '../../services/label.service';
import { LabelChipComponent } from './label-chip.component';

@Component({
  selector: 'app-label-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, LabelChipComponent],
  template: `
    <div class="label-selector">
      <div class="selected-labels" *ngIf="selectedLabels().length > 0">
        <app-label-chip
          *ngFor="let label of selectedLabels()"
          [label]="label"
          [removable]="!readonly"
          (labelRemove)="removeLabel(label)">
        </app-label-chip>
      </div>

      <div class="label-input" *ngIf="!readonly">
        <select 
          class="label-select"
          [(ngModel)]="selectedLabelId"
          (change)="onLabelSelected()"
          [disabled]="selectedLabels().length >= 10">
          <option value="">{{ selectedLabels().length >= 10 ? 'Maximum 10 labels' : 'Select or create label...' }}</option>
          <option *ngFor="let label of availableLabels()" [value]="label.id">
            {{ label.name }}
          </option>
        </select>

        <button 
          type="button"
          class="create-label-btn"
          (click)="showCreateForm = !showCreateForm"
          [disabled]="selectedLabels().length >= 10">
          + New Label
        </button>
      </div>

      <div class="create-label-form" *ngIf="showCreateForm && !readonly">
        <input
          type="text"
          class="label-name-input"
          [(ngModel)]="newLabelName"
          placeholder="Label name"
          maxlength="100">
        <input
          type="color"
          class="label-color-input"
          [(ngModel)]="newLabelColor">
        <button 
          type="button"
          class="btn-primary"
          (click)="createLabel()"
          [disabled]="!newLabelName.trim() || creating()">
          {{ creating() ? 'Creating...' : 'Create' }}
        </button>
        <button 
          type="button"
          class="btn-secondary"
          (click)="cancelCreate()">
          Cancel
        </button>
      </div>
    </div>
  `,
  styles: [`
    .label-selector {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .selected-labels {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
    }

    .label-input {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .label-select {
      flex: 1;
      padding: 8px;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-size: 14px;
    }

    .create-label-btn {
      padding: 8px 16px;
      background-color: #f0f0f0;
      border: 1px solid #ccc;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      white-space: nowrap;
    }

    .create-label-btn:hover:not(:disabled) {
      background-color: #e0e0e0;
    }

    .create-label-btn:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .create-label-form {
      display: flex;
      gap: 8px;
      align-items: center;
      padding: 12px;
      background-color: #f9f9f9;
      border-radius: 4px;
    }

    .label-name-input {
      flex: 1;
      padding: 8px;
      border: 1px solid #ccc;
      border-radius: 4px;
      font-size: 14px;
    }

    .label-color-input {
      width: 50px;
      height: 38px;
      border: 1px solid #ccc;
      border-radius: 4px;
      cursor: pointer;
    }

    .btn-primary, .btn-secondary {
      padding: 8px 16px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-size: 14px;
      white-space: nowrap;
    }

    .btn-primary {
      background-color: #007bff;
      color: white;
    }

    .btn-primary:hover:not(:disabled) {
      background-color: #0056b3;
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .btn-secondary {
      background-color: #6c757d;
      color: white;
    }

    .btn-secondary:hover {
      background-color: #545b62;
    }
  `]
})
export class LabelSelectorComponent {
  private labelService = inject(LabelService);

  @Input({ required: true }) allLabels!: LabelModel[];
  @Input() selectedLabelIds: string[] = [];
  @Input() readonly = false;
  @Output() selectionChange = new EventEmitter<string[]>();
  @Output() labelCreated = new EventEmitter<LabelModel>();

  selectedLabels = signal<LabelModel[]>([]);
  availableLabels = signal<LabelModel[]>([]);
  
  selectedLabelId = '';
  showCreateForm = false;
  newLabelName = '';
  newLabelColor = '#3B82F6';
  creating = signal(false);

  ngOnInit(): void {
    this.updateLabels();
  }

  ngOnChanges(): void {
    this.updateLabels();
  }

  private updateLabels(): void {
    const selected = this.allLabels.filter(l => this.selectedLabelIds.includes(l.id));
    const available = this.allLabels.filter(l => !this.selectedLabelIds.includes(l.id));
    
    this.selectedLabels.set(selected);
    this.availableLabels.set(available);
  }

  onLabelSelected(): void {
    if (this.selectedLabelId && !this.selectedLabelIds.includes(this.selectedLabelId)) {
      const newSelection = [...this.selectedLabelIds, this.selectedLabelId];
      this.selectionChange.emit(newSelection);
      this.selectedLabelId = '';
    }
  }

  removeLabel(label: LabelModel): void {
    const newSelection = this.selectedLabelIds.filter(id => id !== label.id);
    this.selectionChange.emit(newSelection);
  }

  createLabel(): void {
    if (!this.newLabelName.trim()) {
      return;
    }

    if (!this.labelService.isValidHexColor(this.newLabelColor)) {
      alert('Invalid color format');
      return;
    }

    this.creating.set(true);

    this.labelService.createLabel({
      name: this.newLabelName.trim(),
      color: this.newLabelColor.toUpperCase()
    }).subscribe({
      next: (label) => {
        this.labelCreated.emit(label);
        this.cancelCreate();
        this.creating.set(false);
        
        // Automatically select the newly created label
        const newSelection = [...this.selectedLabelIds, label.id];
        this.selectionChange.emit(newSelection);
      },
      error: (error) => {
        console.error('Error creating label:', error);
        alert('Failed to create label');
        this.creating.set(false);
      }
    });
  }

  cancelCreate(): void {
    this.showCreateForm = false;
    this.newLabelName = '';
    this.newLabelColor = '#3B82F6';
  }
}
