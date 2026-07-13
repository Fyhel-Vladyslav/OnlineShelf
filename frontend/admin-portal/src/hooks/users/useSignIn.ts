import { useMutation } from "@tanstack/react-query";
import { userApi } from "@/api/users/userApi";
import type { SignInResponse } from "@/api/users/userApi";


export const useSignIn = () => {

  return useMutation<SignInResponse, Error, { email: string; password: string }>({
    mutationFn: ({ email, password }: { email: string; password: string }) =>
      userApi.signIn(email, password),

    onSuccess: (data: SignInResponse) => {
      localStorage.setItem('access_token', data.token);
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