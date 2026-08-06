import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterPatientRequest } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = environment.apiUrl;
  private readonly tokenKey = 'mg_token';
  private readonly userKey = 'mg_user';

  private readonly _token = signal<string | null>(this.readToken());
  private readonly _user = signal<AuthResponse | null>(this.readUser());

  readonly isLoggedIn = computed(() => !!this._token());
  readonly currentUser = computed(() => this._user());
  readonly roles = computed(() => this._user()?.roles ?? []);
  readonly isPatient = computed(() => this.roles().includes('Patient'));
  readonly isAgent = computed(() => this.roles().includes('Agent'));
  readonly isAdmin = computed(() => this.roles().includes('Admin'));

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Optional: restore user from token later
  }

  login(dto: LoginRequest) {
    return this.http.post<AuthResponse>(`${this.api}/auth/login`, dto).pipe(
      tap((res) => this.setSession(res))
    );
  }

  registerPatient(dto: RegisterPatientRequest) {
    return this.http.post<AuthResponse>(`${this.api}/auth/register-patient`, dto).pipe(
      tap((res) => this.setSession(res))
    );
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userKey);
    this.router.navigateByUrl('/login');
  }

  getToken(): string | null {
    return this._token();
  }


  private setSession(res: AuthResponse) {
    localStorage.setItem(this.tokenKey, res.token);
    localStorage.setItem(this.userKey, JSON.stringify(res));
    this._token.set(res.token);
    this._user.set(res);
  }

  private readToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  private readUser(): AuthResponse | null {
    const raw = localStorage.getItem(this.userKey);
    return raw ? JSON.parse(raw) : null;
  }
}