FROM mcr.microsoft.com/dotnet/sdk:5.0.406-bullseye-slim as builder
COPY ./ /code
WORKDIR /code
RUN dotnet restore

ARG TARGETARCH

# Hack because buildx passes the arch, but it's different to the one that dotnet publish needs 
RUN if [ "$TARGETARCH" = "amd64" ]; \
    then export ARCH=x64; \
    else export ARCH=$TARGETARCH; \
    fi; \
    dotnet publish -c Release --self-contained -r "linux-$ARCH" -o "packages/linux/" -p:PublishSingleFile=true -p:PublishTrimmed=true -p:InvariantGlobalization=true -p:DebugType=None -p:DebugSymbols=false
 
FROM debian
ARG TARGETARCH
COPY --from=builder ./code/packages/linux/SMBeagle /bin/smbeagle

RUN chmod +x /bin/smbeagle

CMD ["smbeagle"]