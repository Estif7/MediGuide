import { Injectable, inject, signal } from '@angular/core';
import { AgentService, AgentDto } from './agent';
import { CategoryService } from './category';
import { ServiceCategory } from '../models/service-category.model';

@Injectable({ providedIn: 'root' })
export class ReferenceDataService {
  private readonly agentService = inject(AgentService);
  private readonly categoryService = inject(CategoryService);

  agents = signal<AgentDto[]>([]);
  categories = signal<ServiceCategory[]>([]);

  private agentsLoaded = false;
  private categoriesLoaded = false;

  loadAgents(force = false) {
    if (this.agentsLoaded && !force) return;

    this.agentService.getAll({ pageSize: 100 }).subscribe({
      next: (result) => {
        this.agents.set(result.items);
        this.agentsLoaded = true;
      },
    });
  }

  loadCategories(force = false) {
    if (this.categoriesLoaded && !force) return;

    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories.set(data);
        this.categoriesLoaded = true;
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