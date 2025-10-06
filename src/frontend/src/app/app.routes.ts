import { Routes } from '@angular/router';
import { GraphComponent } from './graph/graph.component';

export const routes: Routes = [
  { path: '', component: GraphComponent },
  { path: '**', redirectTo: '' }
];
