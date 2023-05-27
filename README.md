# Kanelson

## O que é?
Kanelson é uma aplicação de quiz, inspirada no Kahoot, porém free e open source.

## Como rodar?
### Integração github
A aplicação utiliza autenticação do github para identificar os usuários. Para rodar, é necessário fornecer um ClientId e um ClientSecret.
Segue como configurar:
https://scribehow.com/shared/Adicionar_um_aplicativo_oauth_ao_github__39rxHtPjTRaEgOCZ-vPCfA

Após configurado um aplicativo oauth no github, na raiz do projeto, execute os comandos a seguir, substituindo os valores pelos fornecidos pelo github:
```
dotnet user-secrets set "GithubAuth:ClientSecret" "seuClientSecret" --project ".\Kanelson\"
dotnet user-secrets set "GithubAuth:ClientId" "seuClientId" --project ".\Kanelson\"
```
Dessa forma, suas credenciais não são expostas.

### Localmente

- Azurite - A aplicação depende de tablestorage e blobstorage para a persistência. Ambos já estão configurados no appsettings. Rodando o azurite, já deve funcionar.

### Produção
A aplicação está configurada para utilizar a plataforma Azure, mais especificamente o AppConfiguration, para obter as connections apropriadas.
Utiliza managed identity para se autenticar com os serviços do azure, portanto não contém nenhuma connection string com senha.


Acesse a aplicação através do link [https://kanelson.yanpitangui.com](https://kanelson.yanpitangui.com).

## Arquitetura da aplicação

Essa aplicação utiliza [akka.net](https://getakka.net/), um conjunto de bibliotecas que ajuda a fazer aplicação distribuidas e resilientes que vão desde cores do processador até entre redes,
através da utilização do actor model ([clique para saber mais](https://en.wikipedia.org/wiki/Actor_model#:~:text=The%20actor%20model%20in%20computer%20science%20is%20a,how%20to%20respond%20to%20the%20next%20message%20received)) 







