// Polyfill simple para FormData en React Native
if (typeof global.FormData === 'undefined') {
  class SimpleFormData {
    private data: Array<[string, string | Blob]> = [];

    append(name: string, value: string | Blob): void {
      this.data.push([name, value]);
    }

    get(name: string): string | Blob | null {
      const item = this.data.find(([key]) => key === name);
      return item ? item[1] : null;
    }

    has(name: string): boolean {
      return this.data.some(([key]) => key === name);
    }

    set(name: string, value: string | Blob): void {
      this.delete(name);
      this.append(name, value);
    }

    delete(name: string): void {
      this.data = this.data.filter(([key]) => key !== name);
    }

    entries(): IterableIterator<[string, string | Blob]> {
      return this.data[Symbol.iterator]();
    }

    keys(): IterableIterator<string> {
      return this.data.map(([key]) => key)[Symbol.iterator]();
    }

    values(): IterableIterator<string | Blob> {
      return this.data.map(([, value]) => value)[Symbol.iterator]();
    }

    forEach(callback: (value: string | Blob, key: string, formData: SimpleFormData) => void): void {
      this.data.forEach(([key, value]) => callback(value, key, this));
    }

    [Symbol.iterator](): IterableIterator<[string, string | Blob]> {
      return this.entries();
    }
  }

  // @ts-ignore
  global.FormData = SimpleFormData;
}