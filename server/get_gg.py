import gdown
import os
import zipfile

def get_gg(id, output=None, is_zip=False):
    url = f"https://drive.google.com/uc?id={id}"
    gdown.download(url, output, quiet=False)

    if is_zip and output:
        try:
            with zipfile.ZipFile(output, 'r') as zip_ref:
                zip_ref.extractall(os.path.dirname(output))
            os.remove(output)
            print(f"Unzip {output} success")
        except FileNotFoundError:
            print(f"Error: {output} not found.")
        except zipfile.BadZipFile:
            print(f"Error: {output} is not a valid zip file.")


    print(f"Download {output} success")