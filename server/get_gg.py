import gdown
import os
def get_gg(id, output=None, is_zip=False):
    url = f"https://drive.google.com/uc?id={id}"
    if is_zip is False:
        gdown.download(url, output, quiet=False)

    else:
        gdown.download(url, output, quiet=False)
        os.system(f"unzip {output} -d {os.path.dirname(output)}")
        os.remove(output)
        print(f"Unzip {output} success")

    print(f"Download {output} success")