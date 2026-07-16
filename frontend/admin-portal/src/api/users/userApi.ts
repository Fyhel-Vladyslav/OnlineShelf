import { httpClient } from "../httpClient";

export type UserDto = {
  id: string;
  login: string;
  email: string;
  dateCreated: string;
  updatedAt: string;
  roles: string[];
  emailVerified: boolean;
  state: number;
  avatar: string | null;
};

export type GetUsersResponse = {
  users: UserDto[];
};

export type TokenResponse = {
  accessToken: string;
  refreshToken: string;
};

export const userApi = {
  getUsers: () =>
    httpClient.get<GetUsersResponse>("/users/all"),

  getUserById: (userId: string) =>
    httpClient.post<UserDto>("/users", {
      userId,
    }),

  UpdateUser: (newUser: UserDto) =>
    httpClient.post<UserDto>("/users/update-user", {
      newUser,
    }),
    
  deleteUser: (userId: string) =>
    httpClient.delete<UserDto>(`/users/${userId}`),

  signIn: async (login: string, password: string) => {
    const response = await httpClient.post<TokenResponse>("/users/sign-in", {
      login, 
      password
    });
    return response.data;
  },
};