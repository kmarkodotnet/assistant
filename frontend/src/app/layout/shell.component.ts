import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar.component';
import { SidebarComponent } from './sidebar.component';
import { BottomNavComponent } from './bottom-nav.component';
import { OfflineOverlayComponent } from './offline-overlay.component';
import { HeartbeatService } from '../core/realtime/heartbeat.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, SidebarComponent, BottomNavComponent, OfflineOverlayComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (heartbeat.isOffline()) {
      <app-offline-overlay />
    } @else {
      <div class="flex flex-col h-screen">
        <app-navbar />
        <div class="flex flex-1 overflow-hidden">
          <app-sidebar class="hidden md:flex" />
          <main class="flex-1 overflow-y-auto p-4 md:p-6">
            <router-outlet />
          </main>
        </div>
        <app-bottom-nav class="md:hidden" />
      </div>
    }
  `,
})
export class ShellComponent {
  heartbeat = inject(HeartbeatService);
}
