import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslationService } from '../../../core/services/translation';
import { ThemeService } from '../../../core/services/theme';
import { AuthService } from '../../../core/services/auth';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './about.html',
  styleUrl: './about.scss',
})
export class About {
  readonly i18n = inject(TranslationService);
  readonly themeService = inject(ThemeService);
  private readonly auth = inject(AuthService);

  isLoggedIn = this.auth.isLoggedIn;

  getDashboardLink(): string {
    const roles = this.auth.roles();
    if (roles.includes('Admin')) return '/admin';
    if (roles.includes('Agent')) return '/agent';
    return '/patient';
  }
}
