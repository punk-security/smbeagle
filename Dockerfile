FROM debian
ARG TARGETARCH
COPY packages/linux/$TARGETARCH /bin/smbeagle

RUN chmod +x /bin/smbeagle

CMD ["smbeagle"]
