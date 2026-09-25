import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PopupToast } from './shared/popup-toast/popup-toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PopupToast],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('medi-guide-web');
}
