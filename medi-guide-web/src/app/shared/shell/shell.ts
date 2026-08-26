import { Component, inject, computed } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, MatToolbarModule, MatButtonModule],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(AuthService);

  user = this.auth.currentUser;
  isPatient = this.auth.isPatient;
  isAgent = this.auth.isAgent;
  isAdmin = this.auth.isAdmin;

  homeLink = computed(() => {
    if (this.isAdmin()) return '/admin';
    if (this.isAgent()) return '/agent';
    return '/patient';
  });

  logout() {
    this.auth.logout();
  }
}