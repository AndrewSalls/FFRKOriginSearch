# FFRKOriginSearch
A tool for finding missing data from a Google Sheets "database" and visually listing what data is missing.

You probably shouldn't download this yourself, since it is designed around the very specific needs I had for my Google Sheet.
It relies on the specific text formatting I used in my sheet, and the website scraper I use to find which information is missing relies on the specific format of said website.
Finally, I explicitly added the (now public) Google Sheet I am editing via its Google sheet ID.

The files are also missing the credentials.json file needed to validate a user in order to read from Google Sheets using the official API, although that file might be generated
by the OAuth built in to the API.

Also the website this connected to is now dead along with FFRK (Rip)
