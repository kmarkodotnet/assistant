import { Injectable, signal, inject, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class HeartbeatService implements OnDestroy {
  private http = inject(HttpClient);
  private failCount = 0;
  private intervalId?: ReturnType<typeof setInterval>;

  readonly isOffline = signal(false);

  start(): void {
    this.intervalId = setInterval(() => void this.doPing(), 60_000);
    void this.doPing();
  }

  ngOnDestroy(): void {
    if (this.intervalId) clearInterval(this.intervalId);
  }

  retry(): void {
    this.failCount = 0;
    void this.doPing();
  }

  private async doPing(): Promise<void> {
    try {
      await firstValueFrom(this.http.get('/api/v1/system/heartbeat'));
      this.failCount = 0;
      this.isOffline.set(false);
    } catch {
      this.failCount++;
      if (this.failCount >= 3) {
        this.isOffline.set(true);
      }
    }
  }
}
