import { Component, Input, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { UseCase, DisplayMode } from '../graph.service';
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

  get stepSummary(): string {
    const count = this.useCase.steps.length;
    return `${count} step${count !== 1 ? 's' : ''}`;
  }
}
