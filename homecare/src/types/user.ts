export interface User {
    sub: string; // Subject (User ID)
    username: string; // Username
    email: string;
    nameid: string; // User ID
    role: string; // User role (Patient or Employee)
    jti: string;
    iat: number;
    exp: number;
    iss: string;
    aud: string;
}