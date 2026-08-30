import { Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-state-panel',
  standalone: true,
  imports: [MatProgressSpinnerModule, MatIconModule, MatButtonModule],
  templateUrl: './state-panel.html',
  styleUrl: './state-panel.scss',
})
export class StatePanel {
  loading = input(false);
  error = input<string | null>(null);
  empty = input(false);
  emptyMessage = input('Nothing here yet.');
  emptyIcon = input('inbox');
  retry = input<(() => void) | null>(null);
}