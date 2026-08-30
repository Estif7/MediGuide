import { Injectable, inject, signal } from '@angular/core';
import { AgentService, AgentDto } from './agent';
import { CategoryService } from './category';
import { ServiceCategory } from '../models/service-category.model';

@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly agentService = inject(AgentService);
  private readonly categoryService = inject(CategoryService);

  agents = signal<AgentDto[]>([]);
  agentsLoading = signal(false);
  agentsError = signal<string | null>(null);

  categories = signal<ServiceCategory[]>([]);
  categoriesLoading = signal(false);
  categoriesError = signal<string | null>(null);

  private agentsLoaded = false;
  private categoriesLoaded = false;

  loadAgents(force = false) {
    if (this.agentsLoaded && !force) return;
    if (this.agentsLoading()) return; // avoid overlapping requests

    this.agentsLoading.set(true);
    this.agentsError.set(null);

    this.agentService.getAll({ pageSize: 100 }).subscribe({
      next: (result) => {
        this.agents.set(result.items);
        this.agentsLoaded = true;
        this.agentsLoading.set(false);
      },
      error: () => {
        this.agentsError.set('Failed to load agents');
        this.agentsLoading.set(false);
      },
    });
  }

  loadCategories(force = false) {
    if (this.categoriesLoaded && !force) return;
    if (this.categoriesLoading()) return;

    this.categoriesLoading.set(true);
    this.categoriesError.set(null);

    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.categoriesLoaded = true;
        this.categoriesLoading.set(false);
      },
      error: () => {
        this.categoriesError.set('Failed to load categories');
        this.categoriesLoading.set(false);
      },
    });
  }

  refreshAgents() {
    this.loadAgents(true);
  }

  refreshCategories() {
    this.loadCategories(true);
  }
}