import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  public toBuyList: Item[] = [];

  private readonly defaultItem: Item = {
    id: '00000000-0000-0000-0000-000000000000',
    name: '',
    amount: 1,
    isImportant: false,
    sortOrder: 0,
    listType: 0,
    currentSortMode: 0
  };

  protected newItem: Item = { ...this.defaultItem };

  private http: HttpClient;

  private baseUrl: string;

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;
    this.refreshLists();
  }

  refreshLists() {
    this.http.get<Item[]>(`${this.baseUrl}api/ShoppingList/ToBuy`).subscribe({
      next: (result) => {
        this.toBuyList = result;
      },
      error: (error) => console.error(error)
    });
  }

  addItemToBuy() {
    this.http.post<Item>(`${this.baseUrl}api/ShoppingList/AddItemToBuy`, this.newItem).subscribe({
      next: (result) => {
        this.newItem = { ...this.defaultItem };
        this.refreshLists();
      },
      error: (error) => console.error(error)
    });
  }

  deleteItem(id: string) {
    this.http.delete(`${this.baseUrl}api/ShoppingList/DeleteItem/${id}`).subscribe({
      next: () => this.refreshLists(),
      error: (error) => console.error(error)
    });
  }
}

interface Item {
  id: string;
  name: string;
  amount: number;
  isImportant: boolean;
  sortOrder: number;
  listType: number;
  currentSortMode: number;
}