export class AuthSchema {
  constructor(
    public name: string,
    public email: string,
    public isAuthenticated: boolean = false
  ) {}

  static fromJSON(json: any): AuthSchema {
    return new AuthSchema(
      json.name,
      json.email,
      json.isAuthenticated
    );
  }

  toJSON(): any {
    return {
      name: this.name,
      email: this.email,
      isAuthenticated: this.isAuthenticated
    };
  }
}