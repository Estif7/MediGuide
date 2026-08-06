import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ServiceCategory } from '../models/service-category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  getAll() {
    return this.http.get<ServiceCategory[]>(`${this.api}/servicecategories`);
  }
}