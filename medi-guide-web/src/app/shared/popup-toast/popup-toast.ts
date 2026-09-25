import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { FeedbackService } from '../../core/services/feedback.service';

@Component({
  selector: 'app-popup-toast',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './popup-toast.html',
  styleUrl: './popup-toast.scss'
})
export class PopupToast {
  protected readonly feedback = inject(FeedbackService);

  getIcon(type?: string): string {
    switch (type) {
      case 'success':
        return 'check_circle';
      case 'error':
        return 'error';
      case 'warning':
        return 'warning';
      case 'info':
      default:
        return 'info';
    }
  }

  dismiss() {
    this.feedback.clear();
  }
}
