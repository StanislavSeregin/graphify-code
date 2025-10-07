import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { ServiceData, GraphService, DisplayMode } from '../graph.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-service-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTooltipModule, MatButtonModule],
  templateUrl: './service-card.component.html',
  styleUrl: './service-card.component.css'
})
export class ServiceCardComponent implements OnInit, OnDestroy {
  @Input() serviceData!: ServiceData;

  displayMode: DisplayMode = 'compact';
  isDescriptionExpanded = false;
  private destroy$ = new Subject<void>();

  constructor(private graphService: GraphService) {}

  ngOnInit(): void {
    this.graphService.displayMode$
      .pipe(takeUntil(this.destroy$))
      .subscribe(mode => {
        this.displayMode = mode;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleDescription(event: Event): void {
    event.stopPropagation();
    this.isDescriptionExpanded = !this.isDescriptionExpanded;
  }

  get isExternal(): boolean {
    return this.serviceData.service.relativeCodePath === null;
  }

  get endpointCount(): number {
    return this.serviceData.endpoint.length;
  }

  get useCaseCount(): number {
    return this.serviceData.useCases.length;
  }
}
