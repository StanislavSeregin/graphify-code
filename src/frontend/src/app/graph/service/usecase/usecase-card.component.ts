import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { UseCase, DisplayMode, GraphService } from '../../graph.service';
import { Subject, takeUntil, Observable } from 'rxjs';

@Component({
  selector: 'app-usecase-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTooltipModule, MatButtonModule],
  templateUrl: './usecase-card.component.html',
  styleUrl: './usecase-card.component.css'
})
export class UseCaseCardComponent implements OnInit, OnDestroy {
  @Input() useCase!: UseCase;
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

    // Subscribe to nested graph zoom events to track active use case
    this.graphService.nestedGraphZoom$
      .pipe(takeUntil(this.destroy$))
      .subscribe(zoomRequest => {
        // Only this use case is active if zoom targets it, otherwise inactive
        this.isActive = zoomRequest.targetId === this.useCase.id;
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
    // Focus on use case in nested graph
    this.graphService.requestZoom({
      scope: 'nested',
      targetId: this.useCase.id,
      duration: 750
    });

    // Open use case sidebar if we have serviceData and fullGraph
    if (this.serviceData && this.fullGraph) {
      this.graphService.showUseCaseDetails(this.useCase, this.serviceData, this.fullGraph);
    }
  }

  get stepSummary(): string {
    const count = this.useCase.steps.length;
    return `${count} step${count !== 1 ? 's' : ''}`;
  }
}
