import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { UseCase, UseCaseStep, ServiceData } from '../graph.service';

export interface UseCaseSidebarData {
  useCase: UseCase;
  service: ServiceData;
}

@Component({
  selector: 'app-usecase-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatDividerModule
  ],
  template: `
    <div class="sidebar-container" *ngIf="data">
      <div class="sidebar-header">
        <div class="sidebar-title">
          <h2>{{ data.service.service.name }}</h2>
          <h3>{{ data.useCase.name }}</h3>
        </div>
        <button mat-icon-button (click)="close.emit()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <mat-divider></mat-divider>

      <div class="sidebar-content">
        <!-- Use Case Details -->
        <section class="sidebar-section">
          <h4>Use Case Details</h4>
          <div class="usecase-info">
            <div class="info-item" *ngIf="data.useCase.description">
              <span class="info-label">Description</span>
              <p class="info-value">{{ data.useCase.description }}</p>
            </div>
            <div class="info-item">
              <span class="info-label">Total Steps</span>
              <span class="info-value">{{ data.useCase.steps.length }}</span>
            </div>
          </div>
        </section>

        <mat-divider></mat-divider>

        <!-- Steps -->
        <section class="sidebar-section">
          <h4>Steps</h4>
          <mat-accordion class="steps-accordion">
            <mat-expansion-panel
              *ngFor="let step of data.useCase.steps; let i = index"
              [expanded]="expandedStepIndex === i"
              (opened)="onStepExpanded(i, step)"
              (closed)="onStepClosed(i)">

              <mat-expansion-panel-header>
                <mat-panel-title>
                  <span class="step-number">{{ i + 1 }}</span>
                  <span class="step-name">{{ step.name }}</span>
                </mat-panel-title>
              </mat-expansion-panel-header>

              <div class="step-content">
                <div class="step-info-item" *ngIf="step.description">
                  <span class="step-info-label">Description</span>
                  <p class="step-info-value">{{ step.description }}</p>
                </div>

                <div class="step-info-item" *ngIf="step.relativeCodePath">
                  <span class="step-info-label">Code Path</span>
                  <span class="step-info-value code-path">{{ step.relativeCodePath }}</span>
                </div>
              </div>
            </mat-expansion-panel>
          </mat-accordion>
        </section>
      </div>
    </div>
  `,
  styles: [`
    .sidebar-container {
      height: 100%;
      display: flex;
      flex-direction: column;
      background-color: #ffffff;
      border-right: 1px solid #e0e0e0;
    }

    .sidebar-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 20px;
      background: linear-gradient(135deg, #8a2be2 0%, #6a1bb2 100%);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .sidebar-title h2 {
      margin: 0;
      font-size: 18px;
      font-weight: 600;
      color: #ffffff;
    }

    .sidebar-title h3 {
      margin: 6px 0 0 0;
      font-size: 14px;
      font-weight: 500;
      color: #ffffff;
      opacity: 0.9;
    }

    .sidebar-header button {
      color: #ffffff;
      opacity: 0.9;
    }

    .sidebar-header button:hover {
      opacity: 1;
      background-color: rgba(255, 255, 255, 0.1);
    }

    .sidebar-content {
      flex: 1;
      overflow-y: auto;
      overflow-x: hidden;
      padding: 20px;
      background-color: #e8e8e8;
      max-height: calc(100vh - 100px);
    }

    .sidebar-content::-webkit-scrollbar {
      width: 8px;
    }

    .sidebar-content::-webkit-scrollbar-track {
      background: #d0d0d0;
    }

    .sidebar-content::-webkit-scrollbar-thumb {
      background: #999;
      border-radius: 4px;
    }

    .sidebar-content::-webkit-scrollbar-thumb:hover {
      background: #777;
    }

    .sidebar-section {
      margin-bottom: 28px;
      background-color: #ffffff;
      border-radius: 8px;
      padding: 16px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
    }

    .sidebar-section h4 {
      margin: 0 0 16px 0;
      font-size: 13px;
      font-weight: 600;
      color: #8a2be2;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      border-bottom: 2px solid #8a2be2;
      padding-bottom: 8px;
    }

    .usecase-info {
      display: flex;
      flex-direction: column;
      gap: 14px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .info-label {
      font-size: 11px;
      font-weight: 600;
      color: #666;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .info-value {
      font-size: 14px;
      color: #333;
      margin: 0;
      line-height: 1.5;
      white-space: pre-line;
    }

    .steps-accordion {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    mat-expansion-panel {
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1) !important;
      border-radius: 4px !important;
      border: 1px solid #e0e0e0;
    }

    mat-expansion-panel:hover {
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.15) !important;
    }

    mat-expansion-panel-header {
      background-color: #f8f9fa !important;
    }

    mat-expansion-panel-header:hover {
      background-color: #f0f1f3 !important;
    }

    mat-panel-title {
      display: flex;
      align-items: center;
      gap: 12px;
      flex: 1;
    }

    .step-number {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      background-color: #8a2be2;
      color: #ffffff;
      font-size: 12px;
      font-weight: 600;
      flex-shrink: 0;
    }

    .step-name {
      font-weight: 500;
      color: #333;
      flex: 1;
    }

    .step-content {
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 14px;
    }

    .step-info-item {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .step-info-label {
      font-size: 11px;
      font-weight: 600;
      color: #666;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .step-info-value {
      font-size: 14px;
      color: #333;
      margin: 0;
      line-height: 1.5;
      white-space: pre-line;
    }

    .code-path {
      font-family: 'Courier New', monospace;
      font-size: 12px;
      color: #8a2be2;
      background-color: #f8f0ff;
      padding: 8px;
      border-radius: 4px;
      word-break: break-all;
      border-left: 3px solid #8a2be2;
    }

    .step-links {
      display: flex;
      align-items: center;
      gap: 8px;
      color: #8a2be2;
    }

    .step-links mat-icon {
      width: 18px;
      height: 18px;
      font-size: 18px;
    }

    mat-divider {
      display: none;
    }

    /* Ensure content doesn't overflow sidebar */
    :host {
      display: block;
      width: 100%;
      height: 100%;
      max-width: 100%;
      overflow-x: hidden;
    }
  `]
})
export class UseCaseSidebarComponent {
  @Input() data: UseCaseSidebarData | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() stepClick = new EventEmitter<{step: UseCaseStep, index: number}>();

  expandedStepIndex: number | null = null;

  onStepExpanded(index: number, step: UseCaseStep): void {
    this.expandedStepIndex = index;
    this.stepClick.emit({ step, index });
  }

  onStepClosed(index: number): void {
    if (this.expandedStepIndex === index) {
      this.expandedStepIndex = null;
    }
  }
}
