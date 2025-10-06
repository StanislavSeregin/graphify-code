import { Component, OnInit } from '@angular/core';
import { GraphService } from './graph.service';
import { GraphCanvasComponent } from './graph-canvas.component';

@Component({
  selector: 'app-graph',
  standalone: true,
  imports: [GraphCanvasComponent],
  templateUrl: './graph.component.html',
  styleUrl: './graph.component.css'
})
export class GraphComponent implements OnInit {
  constructor(private graphService: GraphService) {}

  ngOnInit(): void {
    this.graphService.init();
  }
}
