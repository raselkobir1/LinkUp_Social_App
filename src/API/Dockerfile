FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/API/LinkUp.API.csproj", "src/API/"]
COPY ["src/BuildingBlocks/LinkUp.BuildingBlocks.csproj", "src/BuildingBlocks/"]
COPY ["src/SharedKernel/LinkUp.SharedKernel.csproj", "src/SharedKernel/"]
COPY ["src/Modules/Identity/LinkUp.Modules.Identity.csproj", "src/Modules/Identity/"]
COPY ["src/Modules/UserProfile/LinkUp.Modules.UserProfile.csproj", "src/Modules/UserProfile/"]
COPY ["src/Modules/Friend/LinkUp.Modules.Friend.csproj", "src/Modules/Friend/"]
COPY ["src/Modules/Post/LinkUp.Modules.Post.csproj", "src/Modules/Post/"]
COPY ["src/Modules/Comment/LinkUp.Modules.Comment.csproj", "src/Modules/Comment/"]
COPY ["src/Modules/Reaction/LinkUp.Modules.Reaction.csproj", "src/Modules/Reaction/"]
COPY ["src/Modules/Chat/LinkUp.Modules.Chat.csproj", "src/Modules/Chat/"]
COPY ["src/Modules/Notification/LinkUp.Modules.Notification.csproj", "src/Modules/Notification/"]
COPY ["src/Modules/VideoCall/LinkUp.Modules.VideoCall.csproj", "src/Modules/VideoCall/"]
COPY ["src/Modules/Media/LinkUp.Modules.Media.csproj", "src/Modules/Media/"]
COPY ["src/Modules/Search/LinkUp.Modules.Search.csproj", "src/Modules/Search/"]
COPY ["src/Modules/Admin/LinkUp.Modules.Admin.csproj", "src/Modules/Admin/"]
RUN dotnet restore "src/API/LinkUp.API.csproj"
COPY . .
WORKDIR "/src/src/API"
RUN dotnet build "LinkUp.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LinkUp.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LinkUp.API.dll"]
