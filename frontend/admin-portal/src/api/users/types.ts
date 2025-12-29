import type { UserDto } from '@/api/users/userApi';

 export interface UpdateUserCommand {
    payload: UserDto;
  }