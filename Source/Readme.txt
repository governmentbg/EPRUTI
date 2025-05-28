Linux deploy

https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx?view=aspnetcore-7.0&tabs=linux-ubuntu

sudo nano /etc/systemd/system/kestrel-kaisstorage.service
sudo systemctl enable kestrel-kaisstorage.service
sudo systemctl start kestrel-kaisstorage.service
sudo systemctl status kestrel-kaisstorage.service

sudo nano /etc/systemd/system/kestrel-kaisportal.service
sudo systemctl enable kestrel-kaisportal.service
sudo systemctl start kestrel-kaisportal.service
sudo systemctl status kestrel-kaisportal.service

sudo nano /etc/systemd/system/kestrel-kaisoffice.service
sudo systemctl enable kestrel-kaisoffice.service
sudo systemctl start kestrel-kaisoffice.service
sudo systemctl status kestrel-kaisoffice.service

sudo nano /etc/systemd/system/kestrel-kaiswebapi.service
sudo systemctl enable kestrel-kaiswebapi.service
sudo systemctl start kestrel-kaiswebapi.service
sudo systemctl status kestrel-kaiswebapi.service

sudo systemctl daemon-reload

sudo journalctl -fu kestrel-kaisportal.service
sudo journalctl -fu kestrel-kaisportal.service --since today

sudo journalctl -fu kestrel-kaisstorage.service --since today


sudo ln -s /etc/nginx/sites-available/kais.office /etc/nginx/sites-enabled/kais.office
sudo ln -s /etc/nginx/sites-available/kais.portal /etc/nginx/sites-enabled/kais.portal
sudo ln -s /etc/nginx/sites-available/kais.webapi /etc/nginx/sites-enabled/kais.webapi
