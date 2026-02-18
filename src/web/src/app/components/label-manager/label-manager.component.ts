import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LabelService } from '../../services/label.service';
import { LabelModel, CreateLabelRequest, UpdateLabelRequest } from '../../models';

@Component({
  selector: 'app-label-manager',
  imports: [CommonModule, FormsModule],
  templateUrl: './label-manager.component.html',
  styleUrl: './label-manager.component.css'
})
export class LabelManagerComponent implements OnInit {
  private labelService = inject(LabelService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  listId = signal<string>('');
  labels = signal<LabelModel[]>([]);
  loading = signal<boolean>(false);
  error = signal<string>('');
  
  // Form state
  showCreateForm = signal<boolean>(false);
  editingLabel = signal<LabelModel | null>(null);
  formName = signal<string>('');
  formColor = signal<string>('#3498DB');

  // Predefined color palette
  readonly colorPalette = [
    '#E74C3C', // Red
    '#C70039', // Dark Red
    '#FF5733', // Orange Red
    '#FFC300', // Yellow
    '#F39C12', // Orange
    '#2ECC71', // Green
    '#1ABC9C', // Turquoise
    '#3498DB', // Blue
    '#2980B9', // Dark Blue
    '#9B59B6', // Purple
    '#8E44AD', // Dark Purple
    '#34495E', // Dark Gray
    '#95A5A6', // Gray
  ];

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.listId.set(id);
        this.loadLabels();
      }
    });
  }

  loadLabels(): void {
    this.loading.set(true);
    this.error.set('');
    
    this.labelService.getLabels(this.listId()).subscribe({
      next: (labels) => {
        this.labels.set(labels);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load labels:', err);
        this.error.set('Failed to load labels');
        this.loading.set(false);
      }
    });
  }

  openCreateForm(): void {
    this.showCreateForm.set(true);
    this.editingLabel.set(null);
    this.formName.set('');
    this.formColor.set('#3498DB');
  }

  openEditForm(label: LabelModel): void {
    this.editingLabel.set(label);
    this.showCreateForm.set(true);
    this.formName.set(label.name);
    this.formColor.set(label.color);
  }

  closeForm(): void {
    this.showCreateForm.set(false);
    this.editingLabel.set(null);
    this.formName.set('');
    this.formColor.set('#3498DB');
  }

  selectColor(color: string): void {
    this.formColor.set(color);
  }

  saveLabel(): void {
    const name = this.formName().trim();
    const color = this.formColor();

    if (!name) {
      alert('Please enter a label name');
      return;
    }

    const editingLabel = this.editingLabel();
    
    if (editingLabel) {
      // Update existing label
      const request: UpdateLabelRequest = {
        name,
        color
      };
      
      this.labelService.updateLabel(this.listId(), editingLabel.id, request).subscribe({
        next: () => {
          this.loadLabels();
          this.closeForm();
        },
        error: (err) => {
          console.error('Failed to update label:', err);
          alert('Failed to update label: ' + (err.error?.message || err.message));
        }
      });
    } else {
      // Create new label
      const request: CreateLabelRequest = {
        name,
        color
      };
      
      this.labelService.createLabel(this.listId(), request).subscribe({
        next: () => {
          this.loadLabels();
          this.closeForm();
        },
        error: (err) => {
          console.error('Failed to create label:', err);
          alert('Failed to create label: ' + (err.error?.message || err.message));
        }
      });
    }
  }

  deleteLabel(label: LabelModel): void {
    if (confirm(`Are you sure you want to delete the label "${label.name}"?`)) {
      this.labelService.deleteLabel(this.listId(), label.id).subscribe({
        next: () => {
          this.loadLabels();
        },
        error: (err) => {
          console.error('Failed to delete label:', err);
          alert('Failed to delete label: ' + (err.error?.message || err.message));
        }
      });
    }
  }

  goBack(): void {
    this.router.navigate(['/lists', this.listId()]);
  }
}
