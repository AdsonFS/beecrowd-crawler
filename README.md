# beecrowd-crawler

Script para buscar e baixar meus códigos do site beecrowd.

Optei por sempre reescrever o arquivo de cada problema com a ultima submissão de cada problema.

Antes de rodar, não esqueça de coletar o cookie de csrf do beecrowd no header da requisição /judge/en/categories.

Para rodar o script em sua máquina, execute o comando:

```shell
dotnet run --cookie "csrfTokenXXXX%2Fcollect" --lang en
```
