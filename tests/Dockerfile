FROM punksecurity/smbeagle
RUN apt update && apt install python3 python3-pip -y
RUN pip install impacket pytest
RUN mkdir /empty_dir
WORKDIR /tests/
COPY tests/* .
ENTRYPOINT [ "" ]
CMD ["pytest", "-v"]