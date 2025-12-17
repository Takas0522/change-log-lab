import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LabelModel } from '../../models';
import { LabelService } from '../../services/label.service';

@Component({
  selector: 'app-label-chip',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span 
      class="label-chip"
      [style.background-color]="label.color"
      [style.color]="textColor"
      [class.clickable]="clickable"
      [class.removable]="removable"
      (click)="onClick()">
      {{ label.name }}
      <button 
        *ngIf="removable" 
        class="remove-btn"
        (click)="onRemove($event)"
        type="button"
        [attr.aria-label]="'Remove ' + label.name">
        ×
      </button>
    </span>
  `,
  styles: [`
    .label-chip {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 4px 8px;
      border-radius: 12px;
      font-size: 12px;
      font-weight: 500;
      white-space: nowrap;
      transition: opacity 0.2s;
    }

    .label-chip.clickable {
      cursor: pointer;
    }

    .label-chip.clickable:hover {
      opacity: 0.8;
    }

    .label-chip.removable {
      padding-right: 4px;
    }

    .remove-btn {
      background: none;
      border: none;
      color: inherit;
      font-size: 18px;
      line-height: 1;
      padding: 0 4px;
      cursor: pointer;
      opacity: 0.7;
      transition: opacity 0.2s;
    }

    .remove-btn:hover {
      opacity: 1;
    }
  `]
})
export class LabelChipComponent {
  private labelService = inject(LabelService);

  @Input({ required: true }) label!: LabelModel;
  @Input() clickable = false;
  @Input() removable = false;
  @Output() labelClick = new EventEmitter<LabelModel>();
  @Output() labelRemove = new EventEmitter<LabelModel>();

  get textColor(): string {
    return this.labelService.getContrastTextColor(this.label.color);
  }

  onClick(): void {
    if (this.clickable) {
      this.labelClick.emit(this.label);
    }
  }

  onRemove(event: Event): void {
    event.stopPropagation();
    this.labelRemove.emit(this.label);
  }
}
