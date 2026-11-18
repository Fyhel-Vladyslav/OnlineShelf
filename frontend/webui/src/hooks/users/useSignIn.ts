import { useMutation } from "@tanstack/react-query";
import { userApi } from "@/api/users/userApi";
import type { TokenResponse } from "@/api/users/userApi";
import { authService } from "@/hooks/jwtauth/AuthService";

export const useSignIn = () => {

  return useMutation<TokenResponse, Error, { email: string; password: string }>({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      userApi.signIn(email, password),

    onSuccess: (data: TokenResponse) => {
      console.log(data);
      
      authService.setToken(data.accessToken);
      authService.setRefreshToken(data.refreshAccessToken);
    },
    onError: (error, variables) => {
        console.error("signIn failed for:",variables.email);
        console.error(error);
      },
  });

  // return useQuery<string, string[]>({
  //   queryKey: [],
  //   queryFn: async () => {
  //     const res = await userApi.signIn(login, password);
  //     console.log("res",{res});
      
  //     localStorage.setItem('token', res);
  //     return res;
  //   },
  //   staleTime: 1000 * 60, // cache 1 minute
  //   retry: 1, // retry once on failure
  // });
};