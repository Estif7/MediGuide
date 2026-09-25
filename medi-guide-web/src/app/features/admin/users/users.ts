import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { UserService } from '../../../core/services/user';
import { TranslationService } from '../../../core/services/translation';
import { FeedbackService } from '../../../core/services/feedback.service';
import { AdminUser } from '../../../core/models/user.model';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDialogModule,
    MatTooltipModule,
    DatePipe,
  ],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class AdminUsers implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly feedback = inject(FeedbackService);
  private readonly route = inject(ActivatedRoute);
  readonly i18n = inject(TranslationService);

  users = signal<AdminUser[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  actionLoading = signal(false);

  search = signal('');
  roleFilter = signal('');
  page = signal(1);
  pageSize = signal(15);

  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);

  // Safety guard: count active administrators
  activeAdminCount = computed(() =>
    this.users().filter((u) => u.roles?.includes('Admin') && u.isActive).length
  );

  isLastActiveAdmin(user: AdminUser): boolean {
    return !!(user.roles?.includes('Admin') && user.isActive && this.activeAdminCount() <= 1);
  }

  // Modals
  showAddModal = signal(false);
  showEditModal = signal(false);
  editingUser = signal<AdminUser | null>(null);

  // Add Form
  addForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    phoneNumber: [''],
    role: ['Patient', Validators.required],
    title: [''],
    department: ['General Practice'],
    specialty: [''],
  });

  // Edit Form (Role is permanently immutable once account is created)
  editForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(150)]],
    phoneNumber: [''],
    role: ['Patient'],
    isActive: [true],
    title: [''],
    department: ['General Practice'],
    specialty: [''],
  });

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      const role = params['role'];
      const search = params['search'];
      const editEmail = params['editEmail'];

      if (role) {
        this.roleFilter.set(role);
      }
      if (search) {
        this.search.set(search);
      }
      this.loadUsers(editEmail);
    });
  }

  loadUsers(targetEmail?: string) {
    this.loading.set(true);
    this.userService
      .getAll({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search() || undefined,
        role: this.roleFilter() || undefined,
      })
      .subscribe({
        next: (result) => {
          this.users.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);

          if (targetEmail) {
            const match = result.items.find(
              (u) => u.email.toLowerCase() === targetEmail.toLowerCase()
            );
            if (match) {
              this.openEditModal(match);
            }
          }
        },
        error: () => {
          this.loading.set(false);
          const err = this.i18n.isAmharic()
            ? 'ተጠቃሚዎችን መጫን አልተቻለም።'
            : 'Failed to load platform users.';
          this.errorMsg.set(err);
          this.feedback.error(err);
        },
      });
  }

  onSearch(val: string) {
    this.search.set(val);
    this.page.set(1);
    this.loadUsers();
  }

  onRoleFilter(role: string) {
    this.roleFilter.set(role);
    this.page.set(1);
    this.loadUsers();
  }

  openAddModal() {
    this.addForm.reset({
      role: 'Patient',
      department: 'General Practice',
    });
    this.errorMsg.set(null);
    this.showAddModal.set(true);
  }

  closeAddModal() {
    this.showAddModal.set(false);
  }

  submitAdd() {
    if (this.addForm.invalid) return;

    this.actionLoading.set(true);
    const val = this.addForm.getRawValue();

    this.userService
      .create({
        fullName: val.fullName!,
        email: val.email!,
        password: val.password!,
        phoneNumber: val.phoneNumber || undefined,
        role: val.role!,
        title: val.role === 'Agent' ? (val.title || undefined) : undefined,
        department: val.role === 'Agent' ? (val.department || undefined) : undefined,
        specialty: val.role === 'Agent' ? (val.specialty || undefined) : undefined,
      })
      .subscribe({
        next: () => {
          this.actionLoading.set(false);
          this.closeAddModal();
          const success = this.i18n.isAmharic()
            ? 'አዲስ ተጠቃሚ በተሳካ ሁኔታ ተፈጥሯል!'
            : 'User created successfully!';
          this.successMsg.set(success);
          this.feedback.success(success);
          this.loadUsers();
        },
        error: (err) => {
          this.actionLoading.set(false);
          const msg =
            err.error?.[0] ||
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : null) ||
            (this.i18n.isAmharic()
              ? 'ተጠቃሚ መፍጠር አልተቻለም።'
              : 'Failed to create user account.');
          this.errorMsg.set(msg);
          this.feedback.error(msg);
        },
      });
  }

  openEditModal(user: AdminUser) {
    this.editingUser.set(user);
    this.editForm.patchValue({
      fullName: user.fullName,
      phoneNumber: user.phoneNumber || '',
      role: user.roles?.[0] || 'Patient',
      isActive: user.isActive,
      title: user.title || '',
      department: user.department || 'General Practice',
      specialty: user.specialty || '',
    });
    this.errorMsg.set(null);
    this.showEditModal.set(true);
  }

  closeEditModal() {
    this.showEditModal.set(false);
    this.editingUser.set(null);
  }

  submitEdit() {
    if (this.editForm.invalid || !this.editingUser()) return;

    this.actionLoading.set(true);
    const val = this.editForm.getRawValue();
    const user = this.editingUser()!;
    const isAgentUser = user.roles?.includes('Agent');

    // Role is strictly immutable; we omit role from payload
    this.userService
      .update(user.id, {
        fullName: val.fullName!,
        phoneNumber: val.phoneNumber || undefined,
        isActive: val.isActive!,
        title: isAgentUser ? (val.title || undefined) : undefined,
        department: isAgentUser ? (val.department || undefined) : undefined,
        specialty: isAgentUser ? (val.specialty || undefined) : undefined,
      })
      .subscribe({
        next: () => {
          this.actionLoading.set(false);
          this.closeEditModal();
          const success = this.i18n.isAmharic()
            ? 'የተጠቃሚ መረጃ በተሳካ ሁኔታ ተሻሽሏል!'
            : 'User updated successfully!';
          this.successMsg.set(success);
          this.feedback.success(success);
          this.loadUsers();
        },
        error: (err) => {
          this.actionLoading.set(false);
          const msg =
            err.error?.[0] ||
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : null) ||
            (this.i18n.isAmharic()
              ? 'ተጠቃሚን ማስተካከል አልተቻለም።'
              : 'Failed to update user.');
          this.errorMsg.set(msg);
          this.feedback.error(msg);
        },
      });
  }

  toggleActive(user: AdminUser) {
    if (user.isActive && this.isLastActiveAdmin(user)) {
      const warnMsg = this.i18n.isAmharic()
        ? 'የመጨረሻውን ንቁ የአስተዳዳሪ አካውንት ማገድ አይቻልም። ቢያንስ አንድ ንቁ አስተዳዳሪ መኖር አለበት።'
        : 'Cannot deactivate the last active Administrator account. At least one active Administrator must be maintained.';
      this.errorMsg.set(warnMsg);
      this.feedback.warning(warnMsg);
      return;
    }

    const nextStatus = !user.isActive;
    this.userService
      .update(user.id, {
        fullName: user.fullName,
        isActive: nextStatus,
      })
      .subscribe({
        next: () => {
          this.users.update((list) =>
            list.map((u) => (u.id === user.id ? { ...u, isActive: nextStatus } : u))
          );
          const msg = nextStatus
            ? (this.i18n.isAmharic() ? 'ተጠቃሚው ነቅቷል' : 'User activated')
            : (this.i18n.isAmharic() ? 'ተጠቃሚው ታግዷል' : 'User deactivated');
          this.successMsg.set(msg);
          this.feedback.success(msg);
        },
        error: (err) => {
          const msg =
            err.error?.[0] ||
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : null) ||
            (this.i18n.isAmharic()
              ? 'የተጠቃሚ ሁኔታ መቀየር አልተቻለም።'
              : 'Failed to update user status.');
          this.errorMsg.set(msg);
          this.feedback.error(msg);
        },
      });
  }
}
