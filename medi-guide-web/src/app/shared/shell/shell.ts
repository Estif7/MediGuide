import { Component, inject, computed, signal, OnInit, OnDestroy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth';
import { SignalRService } from '../../core/services/signalr';
import { NotificationService } from '../../core/services/notification';
import { NotificationDto } from '../../core/models/notification.model';

const NOTIF_PAGE_SIZE = 5;

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatBadgeModule,
    MatMenuModule,
    MatListModule,
    MatProgressSpinnerModule,
    DatePipe,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly signalR = inject(SignalRService);
  private readonly notificationService = inject(NotificationService);
  private readonly router = inject(Router);

  user = this.auth.currentUser;
  isPatient = this.auth.isPatient;
  isAgent = this.auth.isAgent;
  isAdmin = this.auth.isAdmin;

  notifications = signal<NotificationDto[]>([]);
  unreadCount = signal(0);

  notifPage = signal(1);
  notifTotalCount = signal(0);
  notifLoadingMore = signal(false);
  hasMoreNotifications = computed(
    () => this.notifications().length < this.notifTotalCount()
  );
  unreadBadge = computed(
    () => (this.unreadCount() > 99 ? '99+' : this.unreadCount())
  );

  homeLink = computed(() => {
    if (this.isAdmin()) return '/admin';
    if (this.isAgent()) return '/agent';
    return '/patient';
  });

  ngOnInit() {
    if (!this.auth.isLoggedIn()) return;

    this.signalR.connect();
    this.loadUnreadCount();
    this.loadRecent();

    this.signalR.onNotificationReceived((notification) => {
      this.notifications.update((list) => [notification, ...list]);
      this.notifTotalCount.update((n) => n + 1);
      this.unreadCount.update((n) => n + 1);
    });
  }

  ngOnDestroy() {
    this.signalR.offNotificationReceived();
  }

  loadUnreadCount() {
    this.notificationService.getUnreadCount().subscribe({
      next: (count) => this.unreadCount.set(count),
    });
  }

  loadRecent() {
    this.notifPage.set(1);

    this.notificationService.getAll(1, NOTIF_PAGE_SIZE).subscribe({
      next: (result) => {
        this.notifications.set(result.items);
        this.notifTotalCount.set(result.totalCount);
      },
    });
  }

  loadMoreNotifications() {
    if (this.notifLoadingMore() || !this.hasMoreNotifications()) return;

    const nextPage = this.notifPage() + 1;
    this.notifLoadingMore.set(true);

    this.notificationService.getAll(nextPage, NOTIF_PAGE_SIZE).subscribe({
      next: (result) => {
        this.notifications.update((list) => [...list, ...result.items]);
        this.notifTotalCount.set(result.totalCount);
        this.notifPage.set(nextPage);
        this.notifLoadingMore.set(false);
      },
      error: () => this.notifLoadingMore.set(false),
    });
  }

  collapseNotifications() {
    this.loadRecent();
  }

  onNotificationClick(notification: NotificationDto) {
    if (!notification.isRead) {
      this.notificationService.markRead(notification.id).subscribe({
        next: () => {
          this.notifications.update((list) =>
            list.map((n) => (n.id === notification.id ? { ...n, isRead: true } : n))
          );
          this.unreadCount.update((n) => Math.max(0, n - 1));
        },
      });
    }

    if (notification.bookingId) {
      const path = this.isAdmin() ? '/admin/bookings' : this.isAgent() ? '/agent/bookings' : '/patient/bookings';
      const url = `${path}/${notification.bookingId}`;

      if (this.router.url === url) {
        this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
          this.router.navigate([path, notification.bookingId]);
        });
      } else {
        this.router.navigate([path, notification.bookingId]);
      }
    }
  }

  markAllRead() {
    this.notificationService.markAllRead().subscribe({
      next: () => {
        this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
      },
    });
  }

  logout() {
    this.auth.logout();
  }
}