import { Component, Input, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { Endpoint, DisplayMode } from '../../graph.service';
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
  @Output() focusRequested = new EventEmitter<void>();

  displayMode: DisplayMode = 'compact';
  isDescriptionExpanded = false;
  private destroy$ = new Subject<void>();

  constructor() {}

  ngOnInit(): void {
    if (this.displayMode$) {
      this.displayMode$
        .pipe(takeUntil(this.destroy$))
        .subscribe(mode => {
          this.displayMode = mode;
        });
    }
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
    this.focusRequested.emit();
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
