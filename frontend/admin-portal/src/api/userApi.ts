import { httpClient } from "./httpClient";

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
export const userApi = {
    getUsers: () =>
      httpClient.get<GetUsersResponse>("/users"),

    getById: (userId: string) =>
      httpClient.post<UserDto>("/users/getById", {
        userId,
      }),
  };