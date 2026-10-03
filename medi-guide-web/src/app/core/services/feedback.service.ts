import { Injectable, signal } from '@angular/core';

export interface FeedbackMessage {
  type: 'success' | 'error' | 'info' | 'warning';
  title?: string;
  message: string;
  duration?: number;
}

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  private currentTimeout: any = null;
  readonly activeFeedback = signal<FeedbackMessage | null>(null);

  show(feedback: FeedbackMessage) {
    if (this.currentTimeout) {
      clearTimeout(this.currentTimeout);
      this.currentTimeout = null;
    }

    this.activeFeedback.set(feedback);

    const duration = feedback.duration ?? (feedback.type === 'error' ? 6000 : 4500);
    if (duration > 0) {
      this.currentTimeout = setTimeout(() => {
        this.clear();
      }, duration);
    }
  }

  success(message: string, title: string = 'Success') {
    this.show({ type: 'success', title, message });
  }

  error(message: string, title: string = 'Error') {
    this.show({ type: 'error', title, message });
  }

  info(message: string, title: string = 'Notice') {
    this.show({ type: 'info', title, message });
  }

  warning(message: string, title: string = 'Warning') {
    this.show({ type: 'warning', title, message });
  }

  clear() {
    if (this.currentTimeout) {
      clearTimeout(this.currentTimeout);
      this.currentTimeout = null;
    }
    this.activeFeedback.set(null);
  }
}
