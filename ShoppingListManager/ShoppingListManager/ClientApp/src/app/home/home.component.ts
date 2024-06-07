import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  public toBuyList: Item[] = [];

  public prevBoughtList: Item[] = [];

  protected sortMode: SortMode = SortMode.Alphabetical;

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

    this.http.get<Item[]>(`${this.baseUrl}api/ShoppingList/PrevBought`).subscribe({
      next: (result) => {
        this.prevBoughtList = result;
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

  toggleItemImportance(id: string) {
    this.http.post(`${this.baseUrl}api/ShoppingList/ToggleItemImportance/${id}`, null).subscribe({
      next: () => this.refreshLists(),
      error: (error) => console.error(error)
    });
  }

  moveItemToPrevBought(id: string) {
    this.http.post(`${this.baseUrl}api/ShoppingList/MoveItemToPrevBought/${id}`, null).subscribe({
      next: () => this.refreshLists(),
      error: (error) => console.error(error)
    });
  }

  addItemFromPrevBought(id: string) {
    this.http.post(`${this.baseUrl}api/ShoppingList/AddItemFromPrevBought/${id}`, null).subscribe({
      next: () => this.refreshLists(),
      error: (error) => console.error(error)
    });
  }

  changeSortMode(newMode: number) {
    this.sortMode = newMode;

    this.http.post(`${this.baseUrl}api/ShoppingList/ChangeSortMode/${newMode}`, null).subscribe({
      next: () => this.refreshLists(),
      error: (error) => console.error(error)
    });
  }

  moveItem(id: string, direction: number) {
    // If the user manually changes the sort order of an item, we need to switch to custom sort mode
    this.changeSortMode(SortMode.Custom);

    this.http.post(`${this.baseUrl}api/ShoppingList/MoveItem/${id}/${direction}`, null).subscribe({
      next: () => this.refreshLists(),
      error: (error) => console.error(error)
    });
  }
}

enum SortMode {
  Alphabetical = 0,
  Custom = 1
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