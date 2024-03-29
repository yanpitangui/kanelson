# Kanelson

[![SonarCloud](https://sonarcloud.io/images/project_badges/sonarcloud-white.svg)](https://sonarcloud.io/summary/overall?id=yanpitangui_kanelson)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=coverage)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Lines of Code](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=ncloc)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Duplicated Lines (%)](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=duplicated_lines_density)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=yanpitangui_kanelson&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=yanpitangui_kanelson)
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

- Azurite - `npm install -g azurite` Depois, use `npm run azurite`.
A aplicação depende de tablestorage e blobstorage para a persistência. Ambos já estão configurados no appsettings para as portas padrões. Rodando o azurite, já deve funcionar.

- Dotnet 7.0 - Baixe a ultima versão do sdk aqui: https://dotnet.microsoft.com/en-us/download/dotnet/7.0.

### Produção
A aplicação está configurada para utilizar a plataforma Azure, mais especificamente o AppConfiguration, para obter as connections apropriadas.
Utiliza managed identity para se autenticar com os serviços do azure, portanto não contém nenhuma connection string com senha.
Atualmente roda de maneira containerizada em um Azure Container App. O dockerfile pode ser encontrado em [Dockerfile](Dockerfile).

Acesse a aplicação através do link [https://kanelson.yanpitangui.com](https://kanelson.yanpitangui.com).

## Arquitetura da aplicação

Essa aplicação utiliza [akka.net](https://getakka.net/), um conjunto de bibliotecas que ajuda a fazer aplicação distribuidas e resilientes entre cores do processador e até redes,
através da utilização do actor model ([clique para saber mais](https://en.wikipedia.org/wiki/Actor_model#:~:text=The%20actor%20model%20in%20computer%20science%20is%20a,how%20to%20respond%20to%20the%20next%20message%20received)).

Para o frontend, utilizamos Blazor Server Side. Para mais informações, consulte a [documentação oficial](https://learn.microsoft.com/pt-br/aspnet/core/blazor/hosting-models?view=aspnetcore-7.0).

O Akka.Net nos permite utilizar varios provedores para persistência. Já que a aplicação utilizará serviços azures, 
aproveitei que a solução de storage é barata e por isso utilizamos Azure Storage.

Para os journals, usamos Table Storage. Para os snapshots, Blob Storage.










