import { httpClient } from "./httpClient";

export type RoleDto = {
  id: number;
  name: string;
  description: string;
};

export type GetRolesLisrtResponse = {
  roles: RoleDto[];
};
export const rolesApi = {
    getRolesList: () =>
      httpClient.get<GetRolesLisrtResponse>("/users/get-roles"),


  };