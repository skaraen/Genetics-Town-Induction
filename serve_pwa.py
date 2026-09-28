from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from urllib.parse import urlsplit
import os


class UnityHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith(".js.br"):
            return "application/javascript"
        if path.endswith(".wasm.br"):
            return "application/wasm"
        if path.endswith(".data.br"):
            return "application/octet-stream"
        if path.endswith(".json.br"):
            return "application/json"

        return super().guess_type(path)

    def end_headers(self):
        request_path = urlsplit(self.path).path

        if request_path.endswith(".br"):
            self.send_header("Content-Encoding", "br")

        self.send_header("Cache-Control", "no-store")
        self.send_header("Cross-Origin-Opener-Policy", "same-origin")
        self.send_header("Cross-Origin-Embedder-Policy", "require-corp")

        super().end_headers()


os.chdir("BuildPWA")

server = ThreadingHTTPServer(("localhost", 8001), UnityHandler)
print("Serving Unity PWA at http://localhost:8001")
server.serve_forever()
