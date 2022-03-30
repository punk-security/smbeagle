FROM debian
ARG TARGETARCH
COPY packages/linux/${TARGETARCH}/SMBeagle /bin/smbeagle

RUN chmod +x /bin/smbeagle

ENTRYPOINT ["smbeagle", "-D"]
