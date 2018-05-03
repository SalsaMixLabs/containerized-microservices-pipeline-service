FROM microsoft/mssql-server-linux:2017-latest

# Setup the environment
ENV ACCEPT_EULA "Y"
ENV MSSQL_SA_PASSWORD "Password1!"
EXPOSE 1433

# Copy working files
WORKDIR /usr/src
COPY entrypoint.sh .

CMD [ "/bin/bash", "./entrypoint.sh" ]