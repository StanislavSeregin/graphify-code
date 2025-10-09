import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { Endpoint, DisplayMode, GraphService } from '../../graph.service';
import { Subject, takeUntil, Observable } from 'rxjs';

@Component({
  selector: 'app-endpoint-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTooltipModule, MatButtonModule],
  templateUrl: './endpoint-card.component.html',
  styleUrl: './endpoint-card.component.css'
})
export class EndpointCardComponent implements OnInit, OnDestroy {
  @Input() endpoint!: Endpoint;
  @Input() displayMode$?: Observable<DisplayMode>;
  @Input() serviceData?: any; // ServiceData from parent nested-graph

  displayMode: DisplayMode = 'compact';
  isDescriptionExpanded = false;
  isActive = false;
  private destroy$ = new Subject<void>();
  private fullGraph: any = null;

  constructor(private graphService: GraphService) {}

  ngOnInit(): void {
    if (this.displayMode$) {
      this.displayMode$
        .pipe(takeUntil(this.destroy$))
        .subscribe(mode => {
          this.displayMode = mode;
        });
    }

    // Subscribe to full graph for sidebar opening
    this.graphService.graphData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        this.fullGraph = data;
      });

    // Subscribe to nested graph zoom events to track active endpoint
    this.graphService.nestedGraphZoom$
      .pipe(takeUntil(this.destroy$))
      .subscribe(zoomRequest => {
        // Only this endpoint is active if zoom targets it, otherwise inactive
        this.isActive = zoomRequest.targetId === this.endpoint.id;
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

  onCardClick(event: Event): void {
    // Focus on endpoint in nested graph
    this.graphService.requestZoom({
      scope: 'nested',
      targetId: this.endpoint.id,
      duration: 750
    });

    // Open endpoint sidebar if we have serviceData and fullGraph
    if (this.serviceData && this.fullGraph) {
      this.graphService.showEndpointDetails(this.endpoint, this.serviceData, this.fullGraph);
    }
  }

  get typeIcon(): string {
    switch (this.endpoint.type) {
      case 'http': return 'http';
      case 'queue': return 'mail';
      case 'job': return 'schedule';
      default: return 'help_outline';
    }
  }

  get typeColor(): string {
    switch (this.endpoint.type) {
      case 'http': return '#4A90E2';
      case 'queue': return '#F39C12';
      case 'job': return '#27AE60';
      default: return '#95A5A6';
    }
  }
}
