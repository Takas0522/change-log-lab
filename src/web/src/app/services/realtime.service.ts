import { Injectable, signal, inject } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { ListService } from './list.service';
import { TodoService } from './todo.service';

export interface RealtimeEvent {
  eventId: string;
  eventType: string;
  listId: string;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private readonly authService = inject(AuthService);
  private readonly listService = inject(ListService);
  private readonly todoService = inject(TodoService);
  
  private connection: signalR.HubConnection | null = null;
  private readonly connectedSignal = signal(false);
  private readonly currentListId = signal<string | null>(null);
  
  readonly connected = this.connectedSignal.asReadonly();

  connect() {
    const token = this.authService.token();
    if (!token) {
      console.warn('Cannot connect to SignalR: no auth token');
      return;
    }

    if (this.connection) {
      console.warn('SignalR connection already exists');
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5004/hubs/todo', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('ListUpdated', (event: RealtimeEvent) => {
      console.log('Received ListUpdated event:', event);
      this.handleListUpdate(event);
    });

    this.connection.on('TodoUpdated', (event: RealtimeEvent) => {
      console.log('Received TodoUpdated event:', event);
      this.handleTodoUpdate(event);
    });

    this.connection.onreconnecting(() => {
      console.log('SignalR reconnecting...');
      this.connectedSignal.set(false);
    });

    this.connection.onreconnected(() => {
      console.log('SignalR reconnected');
      this.connectedSignal.set(true);
      this.resubscribeToCurrentList();
    });

    this.connection.onclose(() => {
      console.log('SignalR connection closed');
      this.connectedSignal.set(false);
    });

    this.connection.start()
      .then(() => {
        console.log('SignalR connected');
        this.connectedSignal.set(true);
      })
      .catch(err => {
        console.error('SignalR connection error:', err);
      });
  }

  disconnect() {
    if (this.connection) {
      this.connection.stop();
      this.connection = null;
      this.connectedSignal.set(false);
      this.currentListId.set(null);
    }
  }

  subscribeToList(listId: string) {
    if (!this.connection || !this.connectedSignal()) {
      console.warn('Cannot subscribe: SignalR not connected');
      return;
    }

    this.currentListId.set(listId);
    
    this.connection.invoke('SubscribeToList', listId)
      .then(() => {
        console.log(`Subscribed to list ${listId}`);
      })
      .catch(err => {
        console.error('Error subscribing to list:', err);
      });
  }

  unsubscribeFromList(listId: string) {
    if (!this.connection || !this.connectedSignal()) {
      return;
    }

    if (this.currentListId() === listId) {
      this.currentListId.set(null);
    }

    this.connection.invoke('UnsubscribeFromList', listId)
      .then(() => {
        console.log(`Unsubscribed from list ${listId}`);
      })
      .catch(err => {
        console.error('Error unsubscribing from list:', err);
      });
  }

  private resubscribeToCurrentList() {
    const listId = this.currentListId();
    if (listId !== null) {
      this.subscribeToList(listId);
    }
  }

  private handleListUpdate(event: RealtimeEvent) {
    // Reload the list data when list is updated
    this.listService.loadLists().subscribe();
  }

  private handleTodoUpdate(event: RealtimeEvent) {
    // Reload todos if we're currently viewing this list
    const currentListId = this.currentListId();
    if (currentListId === event.listId) {
      this.todoService.loadTodos(event.listId).subscribe();
    }
  }
}
