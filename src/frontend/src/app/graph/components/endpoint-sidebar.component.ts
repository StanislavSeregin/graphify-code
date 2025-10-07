import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { Endpoint, ServiceData, UseCase } from '../graph.service';

export interface EndpointSidebarData {
  endpoint: Endpoint;
  service: ServiceData;
  relatedServices: ServiceData[];
  useCases: UseCase[];
}

@Component({
  selector: 'app-endpoint-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule
  ],
  template: `
    <div class="sidebar-container" *ngIf="data">
      <div class="sidebar-header">
        <div class="sidebar-title">
          <h2>{{ data.service.service.name }}</h2>
          <h3>{{ data.endpoint.name }}</h3>
        </div>
        <button mat-icon-button (click)="close.emit()">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <mat-divider></mat-divider>

      <div class="sidebar-content">
        <!-- Endpoint Details -->
        <section class="sidebar-section">
          <h4>Endpoint Details</h4>
          <div class="endpoint-info">
            <div class="info-item">
              <span class="info-label">Type</span>
              <span class="info-value">{{ data.endpoint.type }}</span>
            </div>
            <div class="info-item" *ngIf="data.endpoint.description">
              <span class="info-label">Description</span>
              <p class="info-value">{{ data.endpoint.description }}</p>
            </div>
            <div class="info-item" *ngIf="data.endpoint.relativeCodePath">
              <span class="info-label">Path</span>
              <span class="info-value code-path">{{ data.endpoint.relativeCodePath }}</span>
            </div>
          </div>
        </section>

        <mat-divider></mat-divider>

        <!-- Related Services -->
        <section class="sidebar-section" *ngIf="data.relatedServices.length > 0">
          <h4>Related Services</h4>
          <mat-list>
            <mat-list-item
              *ngFor="let service of data.relatedServices"
              class="clickable-item"
              (click)="onServiceClick(service.service.id)">
              <mat-icon matListItemIcon>{{service.service.relativeCodePath ? 'apps' : 'link'}}</mat-icon>
              <div matListItemTitle>{{ service.service.name }}</div>
              <div matListItemLine *ngIf="service.service.description" class="multiline-description">{{ service.service.description }}</div>
            </mat-list-item>
          </mat-list>
        </section>

        <!-- Use Cases -->
        <section class="sidebar-section" *ngIf="data.useCases.length > 0">
          <h4>Use Cases</h4>
          <mat-list>
            <mat-list-item
              *ngFor="let useCase of data.useCases"
              class="clickable-item"
              (click)="onUseCaseClick(useCase.id)">
              <mat-icon matListItemIcon>playlist_play</mat-icon>
              <div matListItemTitle>{{ useCase.name }}</div>
              <div matListItemLine *ngIf="useCase.description" class="multiline-description">{{ useCase.description }}</div>
              <div matListItemLine class="step-count">{{ useCase.steps.length }} steps</div>
            </mat-list-item>
          </mat-list>
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
      border-left: 1px solid #e0e0e0;
    }

    .sidebar-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 20px;
      background: linear-gradient(135deg, #4A90E2 0%, #357ABD 100%);
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
      color: #4A90E2;
      text-transform: uppercase;
      letter-spacing: 0.8px;
      border-bottom: 2px solid #4A90E2;
      padding-bottom: 8px;
    }

    .endpoint-info {
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
    }

    .code-path {
      font-family: 'Courier New', monospace;
      font-size: 12px;
      color: #4A90E2;
      background-color: #f0f7ff;
      padding: 8px;
      border-radius: 4px;
      word-break: break-all;
      border-left: 3px solid #4A90E2;
    }

    .clickable-item {
      cursor: pointer !important;
      transition: all 0.2s;
      border-radius: 4px;
      margin: 4px 0;
    }

    .clickable-item:hover {
      background-color: #e3f2fd !important;
      transform: translateX(4px);
      cursor: pointer !important;
    }

    .clickable-item * {
      cursor: pointer !important;
    }

    .empty-message {
      color: #999;
      font-style: italic;
      margin: 12px 0;
      text-align: center;
      padding: 20px;
      background-color: #f5f5f5;
      border-radius: 4px;
    }

    .step-count {
      font-size: 12px;
      color: #666;
      font-weight: 500;
    }

    mat-divider {
      display: none;
    }

    mat-list {
      padding: 0 !important;
    }

    mat-list-item {
      border-radius: 4px;
      min-height: 64px !important;
      height: auto !important;
    }

    .multiline-description {
      white-space: normal !important;
      line-height: 1.4 !important;
      padding: 4px 0 !important;
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
export class EndpointSidebarComponent {
  @Input() data: EndpointSidebarData | null = null;
  @Output() close = new EventEmitter<void>();
  @Output() serviceClick = new EventEmitter<string>();
  @Output() useCaseClick = new EventEmitter<string>();

  onServiceClick(serviceId: string): void {
    this.serviceClick.emit(serviceId);
  }

  onUseCaseClick(useCaseId: string): void {
    this.useCaseClick.emit(useCaseId);
  }
}
