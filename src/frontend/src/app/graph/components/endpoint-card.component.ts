import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Endpoint, DisplayMode } from '../graph.service';
import { Subject, takeUntil, Observable } from 'rxjs';

@Component({
  selector: 'app-endpoint-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatTooltipModule],
  templateUrl: './endpoint-card.component.html',
  styleUrl: './endpoint-card.component.css'
})
export class EndpointCardComponent implements OnInit, OnDestroy {
  @Input() endpoint!: Endpoint;
  @Input() displayMode$?: Observable<DisplayMode>;

  displayMode: DisplayMode = 'compact';
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

  get typeIcon(): string {
    switch (this.endpoint.type) {
      case 'http': return 'http';
      case 'queue': return 'queue';
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
